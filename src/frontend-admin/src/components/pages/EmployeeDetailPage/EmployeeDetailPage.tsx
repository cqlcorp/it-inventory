import React, {useState, useEffect, useContext} from 'react'
import {AxiosService, URL} from '../../../services/AxiosService/AxiosService'

// Packages
import {cloneDeep} from 'lodash'

// Components
import icon from '../../../content/Images/CQL-favicon.png'
import {DetailPageTable} from '../../reusables/DetailPageTable/DetailPageTable'
import ReactTooltip from 'react-tooltip'
import {IoMdAdd} from 'react-icons/io'
import {Button} from '../../reusables/Button/Button'
import {Group} from '../../reusables/Group/Group'

// Utils
import {sortTable} from '../../../utilities/quickSort'
import {formatDate, getDays, calculateDaysEmployed} from '../../../utilities/FormatDate'
import {format} from '../../../utilities/formatEmptyStrings'
import {concatStyles as s} from '../../../utilities/mikesConcat'

// Styles
import styles from './EmployeeDetailPage.module.css'

// Context
import {LoginContext} from '../../App/App'

// Types
interface IEmployeeDetailPageProps {
    match: any
    history: any
}

// Helpers

// Primary Component
export const EmployeeDetailPage: React.SFC<IEmployeeDetailPageProps> = props => {
    const {history, match} = props

    const {
        loginContextVariables: {accessToken, refreshToken /*, isAdmin*/},
    } = useContext(LoginContext)
    const isAdmin = true //TODO: remove

    const axios = new AxiosService(accessToken, refreshToken)
    const [userData, setUserData] = useState<any>({})
    const [hwdata, setHWData] = useState<any[]>([])
    const [swdata, setSWData] = useState<any[]>([])
    const [ldata, setLData] = useState<any[]>([])

    const hwheaders = ['Hardware', 'Serial Number', 'MFG Tag', 'Purchase Date']
    const swheaders = ['Software', 'Key/Username', 'Monthly Cost']
    const lheaders = ['Licenses', 'CALs']

    const formatToolTip = (obj: any) => obj.cpu + ' | ' + obj.ramgb + 'GB | ' + obj.ssdgb + 'GB'

    useEffect(() => {
        axios
            .get(`/detail/employee/${match.params.id}`)
            .then((data: any) => {
                //console.log(data)
                let user: any = {
                    photo: data[0].picture,
                    name: data[0].firstName + ' ' + data[0].lastName,
                    department: data[0].department,
                    role: data[0].role,
                    hireDate: formatDate(data[0].hireDate),
                    hwCost: Math.round(data[0].totalHardwareCost * 100) / 100,
                    swCost: Math.round(data[0].totalProgramCostPerMonth * 100) / 100,
                }
                setUserData(user)

                let hw: any[] = []
                data[0].hardware.map((i: any) =>
                    hw.push({
                        name: format(i.make + ' ' + i.model),
                        serial: format(i.serialNumber),
                        mfg: format(i.mfg),
                        purchaseDate: formatDate(i.purchaseDate),
                        id: format(i.id),
                        tooltip: i.tooltip.cpu ? formatToolTip(i.tooltip) : '',
                    })
                )
                setHWData(hw)

                let sw: any[] = []
                data[0].software.map((i: any) =>
                    sw.push({
                        name: format(i.name),
                        licenseKey: format(i.licenseKey),
                        costPerMonth: format(Math.round(i.costPerMonth * 100) / 100),
                        flatCost: format(i.flatCost),
                        id: format(i.id),
                    })
                )
                setSWData(sw)

                let l: any[] = []
                data[0].licenses.map((i: any) =>
                    l.push({
                        name: format(i.name),
                        cals: format(i.cals),
                        licenseKey: format(i.licenseKey),
                        costPerMonth: format(Math.round(i.costPerMonth * 100) / 100),
                        flatCost: format(i.flatCost),
                        id: format(i.id),
                    })
                )
                setLData(l)
            })
            .catch((err: any) => console.error(err))
    }, [])

    console.log(URL + userData.photo)

    return (
        <div className={styles.empDetailMain}>
            <div className={styles.columns}>
                {/* column 1 */}
                <div className={styles.firstColumn}>
                    <Button
                        text='All Employees'
                        icon='back'
                        onClick={() => {
                            history.push('/employees')
                        }}
                        className={styles.backButton}
                        textClassName={styles.backButtonText}
                    />
                    <div className={styles.imgPadding}>
                        <img className={styles.img} src={URL + userData.photo} alt={''} />
                    </div>
                    <div className={styles.costText}>
                        <p>Software ---------------- ${userData.swCost} /month</p>
                        <p>Hardware --------------- ${userData.hwCost}</p>
                    </div>
                </div>
                {/* column 2 */}
                <div className={styles.secondColumn}>
                    {isAdmin && (
                        <Group direction='row' justify='start' className={styles.group}>
                            <Button text='Edit' icon='edit' onClick={() => {}} className={styles.editbutton} />

                            <Button text='Archive' icon='archive' onClick={() => {}} className={styles.archivebutton} />
                        </Group>
                    )}
                    <div className={styles.titleText}>
                        <div className={styles.employeeName}>{userData.name}</div>
                        <div className={styles.employeeText}>
                            {userData.department} | {userData.role}
                        </div>
                        <div className={styles.employeeText}>
                            Hired: {userData.hireDate} | {calculateDaysEmployed(getDays(userData.hireDate))}
                        </div>
                    </div>
                    <DetailPageTable headers={hwrenderHeaders()} rows={hwRenderedRows} />
                    {isAdmin && (
                        <Button
                            text='Assign new hardware'
                            icon='add'
                            onClick={() => {}}
                            className={styles.addContainer}
                            textInside={false}
                            textClassName={styles.assignText}
                        />
                    )}

                    <DetailPageTable headers={swrenderHeaders()} rows={swRenderedRows} />
                    {isAdmin && (
                        <Button
                            text='Assign new software'
                            icon='add'
                            onClick={() => {}}
                            className={styles.addContainer}
                            textInside={false}
                            textClassName={styles.assignText}
                        />
                    )}

                    <DetailPageTable headers={lrenderHeaders()} rows={lRenderedRows} />
                    {isAdmin && (
                        <Button
                            text='Assign new license'
                            icon='add'
                            onClick={() => {}}
                            className={styles.addContainer}
                            textInside={false}
                            textClassName={styles.assignText}
                        />
                    )}
                </div>
            </div>
        </div>
    )
}
