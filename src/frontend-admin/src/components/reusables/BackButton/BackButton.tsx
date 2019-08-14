import React, { useContext } from 'react'
import { History } from 'history'
import { concatStyles as s } from '../../../utilities/mikesConcat'

// Components
import { Button } from '../Button/Button'

// styles
import styles from './BackButton.module.css'
import { ThemeContext } from '../../App';

// Types
interface IBackButtonProps {
    history: History
    className?: string
    textClassName?: string
}

// Primary Component
export const BackButton: React.SFC<IBackButtonProps> = props => {
    const { history, className = '', textClassName = '' } = props
    const { isDarkMode } = useContext(ThemeContext)

    var pathArray = history.location.state ? history.location.state.prev.pathname.split('/') : 'Back'
    var text =
        pathArray.length > 2
            ? pathArray[1][pathArray[1].length - 1] === 's'
                ? pathArray[1].substring(0, pathArray[1].length - 1)
                : pathArray[1]
            : pathArray[1] === 'dashboard'
                ? pathArray[1]
                : 'All ' + pathArray[1]

    var prevIsEdit = history.location.state && history.location.state.prev.pathname.search('edit') !== -1
    var prevIsSame = history.location.state.prev.pathname === history.location.pathname;

    if (prevIsEdit || prevIsSame) {
        text = 'All ' + pathArray[1]
    }

    return (
        <Button
            text={history.location.state ? text : pathArray}
            icon='back'
            onClick={() => {
                if (prevIsEdit || prevIsSame) {
                    history.push(`/${pathArray[1]}`)
                } else {
                    history.goBack()
                }
            }}
            className={s(styles.button, className)}
            textClassName={s(textClassName, isDarkMode ? styles.dark : {})}
        />
    )
}
